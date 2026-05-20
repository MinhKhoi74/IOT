import PageMeta from "../../components/common/PageMeta";
import AuthLayout from "./AuthPageLayout";
import SignUpForm from "../../components/auth/SignUpForm";

export default function SignUp() {
  return (
    <>
      <PageMeta
        title="Đăng ký | SmartParking"
        description="Trang đăng ký SmartParking"
      />
      <AuthLayout>
        <SignUpForm />
      </AuthLayout>
    </>
  );
}
