import PageMeta from "../../components/common/PageMeta";
import AuthLayout from "./AuthPageLayout";
import SignInForm from "../../components/auth/SignInForm";

export default function SignIn() {
  return (
    <>
      <PageMeta
        title="Đăng nhập | SmartParking"
        description="Trang đăng nhập SmartParking"
      />
      <AuthLayout>
        <SignInForm />
      </AuthLayout>
    </>
  );
}
